/// Global modal gate for temporary UI overlays (e.g., help panel, dialogs).
public static class UiModalGate
{
    private static int depth;
    public static bool IsBlocked => depth > 0;

    public static void Push() { depth++; }
    public static void Pop()  { if (depth > 0) depth--; }
}
