class Bubblewrap < Formula
  desc "Unprivileged sandboxing tool (Windows AppContainer)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "bwrap", "bwrap"
  end

  def caveats
    <<~EOS
      bwrap runs programs in a Windows AppContainer: they can read Windows and Program Files,
      but none of your files unless you bind them with --bind or --ro-bind (at the same path).
    EOS
  end

  test do
    assert_match "bubblewrap", shell_output("#{bin}/bwrap --version")
    assert_match "hi", shell_output("#{bin}/bwrap -- cmd /c echo hi")
    (testpath/"secret.txt").write "brew"
    dir = testpath.to_s.tr("/", "\\")
    refute_match "brew", shell_output("#{bin}/bwrap -- cmd /c type '#{dir}\\secret.txt' 2>&1", 1)
    assert_match "brew", shell_output("#{bin}/bwrap --ro-bind '#{dir}' '#{dir}' -- cmd /c type '#{dir}\\secret.txt'")
    assert_match "no mount namespaces", shell_output("#{bin}/bwrap --bind '#{dir}' C:/elsewhere cmd 2>&1", 1)
  end
end
