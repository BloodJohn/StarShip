using System;
using System.Collections.Generic;
using System.Text;

/// <summary>Интерфейс позволяющий использовать StarShipCommand</summary>
internal interface IShip
{
    ///<summary>Speed from gShip data (V)</summary>
    int Speed { get; }
    ///<summary>hyperSpeed from gShip data (R = V2/a)</summary>
    int ShiftSpeed { get; }
    /// <summary>Фиксирование обновления статуса объекта (lastUpdate = JSTime.Now;)</summary>
    void CompleteUpdating();
    /// <summary>время последнего обновления объекта</summary>
    long LastUpdate { get; }

    Vector2D GetCurrentPosition(long timeNow);

    bool IsVisible(long timeNow);
}

internal sealed class StarShipCommand
{
    #region static variables
    private const double minAngle = 0.001d;
    /// <summary>Минимальное время для следующего обновления</summary>
    internal const long minTimeStep = 100;
    #endregion

    #region variables
    ///<summary>Create current commant time.</summary>
    private Int64 startTime;
    /// <summary>время последнего обновления команды</summary>
    private Int64 lastUpdate;
    /// <summary>Star current commant point</summary>
    private Vector2D start = new Vector2D(0, 0);
    /// <summary>Finish current commant point</summary>
    private Vector2D destination = new Vector2D(0, 0);
    ///<summary>Start Direction current command</summary>
    internal Vector2D direction = new Vector2D(1, 0);

    internal bool isFlyMode;

    ///<summary>Rotate Direction sign.(-1/0/1)</summary>
    internal int dirRotate;

    ///<summary>radiust of rotate.</summary>
    private double radiusRotate;

    ///<summary>rotate Angle.</summary>
    private double rotateAngle;

    private Vector2D dotRotate = new Vector2D(0,0);

    internal Vector2D curHead = new Vector2D(1, 0);
    private Vector2D curPos = new Vector2D(1, 0);
    ///<summary>last time calc position</summary>
    private Int64 lastCalcPosition;

    ///<summary>Speed from gShip data (V)</summary>
    private int Speed { get { return ship.Speed; } }
    ///<summary>hyperSpeed from gShip data (R = V2/a)</summary>
    private int ShiftSpeed { get { return ship.ShiftSpeed; } }
    /// <summary>Изменение статуса объекта</summary>
    //private readonly object updateStatus = new object();
    #endregion

    #region constructor
    private readonly IShip ship;
    internal StarShipCommand(IShip ship)
    {
        this.ship = ship;
    }
    #endregion

    #region commands
    /// <summary>Функция не коректирует направление корабля! (остается от прежней команды)</summary>
    internal void SetPos(Vector2D newPos)
    {
        //lock (updateStatus)
        {
            ship.CompleteUpdating();

            start = newPos;
            curPos = newPos;
            destination = newPos;
            lastUpdate = ship.LastUpdate;
            startTime = lastUpdate;
            isFlyMode = false;
        }
    }

    //  clientTime|DestX|DestY
    internal void MoveCommand(Vector2D newDestination)
    {
        //нет двигателя - нет команд.
        if ((Speed < 0) || (ShiftSpeed < 0)) return;

        //lock (updateStatus)
        {
            ship.CompleteUpdating();
            lastUpdate = ship.LastUpdate;
            UpdateCurPosition(ship.LastUpdate);
            start = curPos;
            direction = curHead;
            destination = newDestination;

            startTime = lastUpdate;
            CalcNewDestination();

            lastUpdate = ship.LastUpdate;
        }
    }
    #endregion

    #region functions
    /// <summary>Calc command params on server side</summary>
    private void CalcNewDestination()
    {
        Vector2D path = destination - start;

        if (1 > path.Quadratic) { SetPos(destination); return; }

        double dirAngle = direction.GetAngle(path);

        dirRotate = 0;

        if (dirAngle > minAngle) dirRotate = 1;
        if (dirAngle < -minAngle) dirRotate = -1;

        if (0 != dirRotate) //нужен доворот
        {
            if (IsBlindZone())
            {
                radiusRotate = path.Modulus() / (Math.Sin(Math.Abs(dirAngle)) * 2);
            }
            else
            {
                radiusRotate = (double)(Speed * Speed) / ShiftSpeed;
            }

            dotRotate = new Vector2D(
                -direction.Y * dirRotate * radiusRotate,
                direction.X * dirRotate * radiusRotate);

            radiusRotate = dotRotate.Modulus();

            dotRotate = dotRotate + start;

            rotateAngle = GetRotateAngle();
        }

        isFlyMode = true;
    }

    /// <summary>Zone inside ship rotation</summary>
    private bool IsBlindZone()
    {
        double rBlind = (double)(Speed * Speed) / ShiftSpeed;
        Vector2D dotRotate1 = new Vector2D(-direction.Y * rBlind, direction.X * rBlind);

        Vector2D dotRotate2 = destination - (dotRotate1 + start);

        if (dotRotate2.Quadratic <= rBlind * rBlind) return true;

        dotRotate2 = destination - (start - dotRotate1);

        if (dotRotate2.Quadratic <= rBlind * rBlind) return true;

        return false;
    }

    /// <summary>Calc rotation angle</summary>
    private double GetRotateAngle()
    {
        Vector2D rotDest = destination - dotRotate;
        double hypotenuse = rotDest.Modulus();
        double angle = (radiusRotate < hypotenuse) ? Math.Asin(radiusRotate / hypotenuse) : 1;
        Vector2D rotHead = rotDest.Rotate(angle * dirRotate);
        double result = direction.GetAngle(rotHead);

        if (dirRotate > 0)
        {
            while (result < 0) result += Math.PI * 2;
        }
        else
        {
            while (result > 0) result -= Math.PI * 2;
        }

        return result;
    }
    #endregion

    #region get position

    /// <summary>Считает текущую позицию с корректировкой траектории</summary>
    internal Vector2D UpdateCurPosition(long timeNow)
    {
        if (!isFlyMode) return curPos; //корабль неподвижен

        //отсекаем слишком частое обновление
        //if (timeNow < lastCalcPosition + minTimeStep) return curPos;

        //lock (updateStatus)
        {
            //ВНИМАНИЕ, здесь идет модификация команды (упрощение формулы) которая потенциально может заблокировать команду пользователя, оттданную чуть раньше.
            lastCalcPosition = timeNow;
            if (timeNow < startTime)
            {
                curPos = start;
            }
            else
            {
                curPos = destination;

                const double dd = 200.0d;
                double dTime = ((timeNow - startTime) / dd);

                if (0 == dirRotate)
                {
                    MoveOnLine(dTime);
                }
                else
                {
                    //move rotate
                    double dAlfa; //= shiftSpeed * dTime / Speed;  //alfa = V*dT / R = a * dT / V = sqrt(a/R) * dT
                                  //R = V*V/a
                    if (radiusRotate < ((double)(Speed * Speed) / ShiftSpeed)) //rotate by small radius
                        dAlfa = Math.Sqrt(ShiftSpeed / radiusRotate) * dTime;
                    else
                        dAlfa = Speed * dTime / radiusRotate;


                    if (dAlfa < Math.Abs(rotateAngle))
                    {
                        //turn to target
                        curPos = start.Rotate(dirRotate * dAlfa, dotRotate);
                        curHead = direction.Rotate(dirRotate * dAlfa);
                    }
                    else
                    {
                        //turn to dest nad move on line
                        dAlfa = Math.Abs(rotateAngle);
                        curPos = start.Rotate(dirRotate * dAlfa, dotRotate);
                        double rotTime;

                        if (radiusRotate < ((double)(Speed * Speed) / ShiftSpeed)) //rotate by small radius
                            rotTime = Math.Sqrt(radiusRotate / ShiftSpeed) * dAlfa;
                        else
                            rotTime = dAlfa * radiusRotate / Speed;

                        //Vector2D Path = Destination - curPos;
                        curHead = (destination - curPos).Unit();
                        //curHead = direction.Rotate(dirRotate * dAlfa); //посчитаем честно..

                        dirRotate = 0;
                        start = curPos;
                        direction = curHead;

                        dTime -= rotTime;
                        startTime += (long)Math.Round(rotTime * dd);

                        MoveOnLine(dTime);
                    }
                }
            }
        }

        return curPos;
    }

    /// <summary>Считает текущую позицию с корректировкой траектории</summary>
    private void MoveOnLine(double dTime)
    {
        Vector2D path = destination - start;
        double dist = path.Quadratic;

        if ((Speed * dTime) * (Speed * dTime) < dist)
        {
            isFlyMode = true;
            //curPos = start + curHead * (Speed * dTime);
            curPos = start + path * (Speed * dTime / Math.Sqrt(dist));
        }
        else
        {//end moving
            SetPos(destination);
        }
    }

    /// <summary>расчет положения в будущем времени</summary>
    internal Vector2D GetFuturePosition(long timeFuture)
    {
        if (timeFuture < startTime) return start;
        if (!isFlyMode) return destination;

        const double dd = 200.0d;
        double dTime = ((timeFuture - startTime) / dd);
        if (0 == dirRotate) return GetMoveLine(dTime, start);

        //move rotate
        double dAlfa; //= shiftSpeed * dTime / Speed;  //alfa = V*dT / R = a * dT / V = sqrt(a/R) * dT
                      //R = V*V/a
        if (radiusRotate < ((double)(Speed * Speed) / ShiftSpeed)) //rotate by small radius with max shiftSpeed
            dAlfa = Math.Sqrt(ShiftSpeed / radiusRotate) * dTime;
        else
            dAlfa = Speed * dTime / radiusRotate;

        //just turn to target
        if (dAlfa < Math.Abs(rotateAngle)) return start.Rotate(dirRotate * dAlfa, dotRotate);


        //turn to dest and move on line
        dAlfa = Math.Abs(rotateAngle);
        Vector2D result = start.Rotate(dirRotate * dAlfa, dotRotate);
        double rotTime;

        if (radiusRotate < ((double)(Speed * Speed) / ShiftSpeed)) //rotate by small radius
            rotTime = Math.Sqrt(radiusRotate / ShiftSpeed) * dAlfa;
        else
            rotTime = dAlfa * radiusRotate / Speed;


        dTime -= rotTime;
        return GetMoveLine(dTime, result);
    }

    /// <summary>Считает позицию без корректировки траектории</summary>
    private Vector2D GetMoveLine(double dTime, Vector2D startLine)
    {
        Vector2D path = destination - startLine;
        double dist = path.Quadratic;

        if ((Speed * dTime) * (Speed * dTime) < dist) return startLine + path * (Speed * dTime / Math.Sqrt(dist));

        //end moving
        return destination;
    }
    #endregion

    #region state functions
    /*internal StarObjectAttribute GetStatus(Int64 timeNow)
    {
        StringBuilder result = new StringBuilder();
        lock (updateStatus)
        {
            result.Append("starttime=\"" + (startTime - timeNow) + "\" ");
            result.Append("start=\"" + start.IntX + "," + start.IntY + "\" ");
            result.Append("destination=\"" + destination.IntX + "," + destination.IntY + "\" ");
            result.Append("direction=\"" + direction.X.ToString(System.Globalization.NumberFormatInfo.InvariantInfo)
                          + "," + direction.Y.ToString(System.Globalization.NumberFormatInfo.InvariantInfo) + "\" ");
        }

        return new StarObjectAttribute(result.ToString(), lastUpdate);
    }*/
    #endregion
}
