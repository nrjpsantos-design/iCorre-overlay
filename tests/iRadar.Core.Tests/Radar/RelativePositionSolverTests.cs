using iRadar.Core.Radar;
using Xunit;

namespace iRadar.Core.Tests.Radar;

public class RelativePositionSolverTests
{
    [Fact]
    public void Solve_CarAhead_PlacesAtPositiveX()
    {
        var (x, y) = RelativePositionSolver.Solve(
            playerLapDistPct: 0.10f,
            otherLapDistPct: 0.11f,
            trackLengthMeters: 5000f,
            lateralHint: LateralHint.None,
            sideBySideLateralMeters: 3.5f);

        Assert.Equal(50f, x, precision: 2);
        Assert.Equal(0f, y);
    }

    [Fact]
    public void Solve_CarBehind_PlacesAtNegativeX()
    {
        var (x, _) = RelativePositionSolver.Solve(0.20f, 0.19f, 5000f, LateralHint.None, 3.5f);
        Assert.Equal(-50f, x, precision: 2);
    }

    [Fact]
    public void Solve_LeftHint_PlacesAtNegativeY()
    {
        var (_, y) = RelativePositionSolver.Solve(0.50f, 0.501f, 5000f, LateralHint.Left, 3.5f);
        Assert.Equal(-3.5f, y);
    }

    [Fact]
    public void Solve_RightHint_PlacesAtPositiveY()
    {
        var (_, y) = RelativePositionSolver.Solve(0.50f, 0.501f, 5000f, LateralHint.Right, 3.5f);
        Assert.Equal(+3.5f, y);
    }

    [Fact]
    public void Solve_CarsOnSameLapDist_ButLeftHint_StillPlacesLaterally()
    {
        var (x, y) = RelativePositionSolver.Solve(0.50f, 0.50f, 5000f, LateralHint.Left, 3.5f);
        Assert.Equal(0f, x);
        Assert.Equal(-3.5f, y);
    }

    // ----- InferLateralHint -----

    [Fact]
    public void InferLateralHint_FarCarsDontGetSideHint()
    {
        var hint = RelativePositionSolver.InferLateralHint(
            x: 50f, SpotterAlert.CarLeft, sideBySideMaxLongitudinalMeters: 10f);
        Assert.Equal(LateralHint.None, hint);
    }

    [Theory]
    [InlineData(SpotterAlert.CarLeft, LateralHint.Left)]
    [InlineData(SpotterAlert.TwoCarsLeft, LateralHint.Left)]
    [InlineData(SpotterAlert.CarRight, LateralHint.Right)]
    [InlineData(SpotterAlert.TwoCarsRight, LateralHint.Right)]
    public void InferLateralHint_NearCar_MapsSpotterAlertToSide(SpotterAlert spotter, LateralHint expected)
    {
        var hint = RelativePositionSolver.InferLateralHint(
            x: 3f, spotter, sideBySideMaxLongitudinalMeters: 10f);
        Assert.Equal(expected, hint);
    }

    [Theory]
    [InlineData(SpotterAlert.Clear)]
    [InlineData(SpotterAlert.CarLeftRight)]   // ambiguous — both sides
    public void InferLateralHint_ClearOrAmbiguous_ReturnsNone(SpotterAlert spotter)
    {
        var hint = RelativePositionSolver.InferLateralHint(x: 3f, spotter, 10f);
        Assert.Equal(LateralHint.None, hint);
    }
}
