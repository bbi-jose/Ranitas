﻿namespace Ranitas.Data
{
    public sealed class FrogData
    {
        public float Width;
        public float Height;

        public FrogMovementData MovementData;

        public float WaterDrag;
        public float FrogDensity;

        public float SwimKickDuration;
        public float SwimKickRecharge;
        public float SwimKickVelocity;

        public float ToungueLength;
        public float ToungueThickness;
        public float ToungueRelativeVerticalOffset;
        public float ToungueExtendTime;
        public float ToungueFullyExtendedTime;
        public float ToungueRetractTime;
        public float ToungueRefreshTime;
    }

    public sealed class FrogMovementData
    {
        public float JumpVelocity;
        public float JumpPrepareTime;
        public float JumpSquish;
    }
}
