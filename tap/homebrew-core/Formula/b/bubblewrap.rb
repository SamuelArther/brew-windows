class Bubblewrap < Formula
  desc "Unprivileged sandboxing tool (Windows AppContainer)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

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
