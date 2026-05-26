using EMILtools.Systems;
using static PlayerController;

public class PrimaryInputAuthority : InputAuthority<
    PlayerInputReader,
    PlayerInputMap,
    PrimaryInputAuthority.Subordinates>
{
    public enum Subordinates
    {
        Player
    }
}