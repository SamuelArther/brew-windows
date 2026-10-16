class Libnfnetlink < Formula
  desc "Interface index table tool on IP Helper (nfnl-iftest)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "nfnl-iftest", "nfnl-iftest"
  end

  def caveats
    <<~EOS
      nfnl-iftest is libnfnetlink's iftest: it prints the interface index table nfnetlink keeps
      (index, name, up/down) and, with -m, its changes. There is no libnfnetlink C library on Windows:
      programs use iphlpapi and the Windows Filtering Platform.
    EOS
  end

  test do
    assert_match(/index \(\d+\) is .* \((up|down)\)/, shell_output("#{bin}/nfnl-iftest"))
    assert_match "Usage: nfnl-iftest", shell_output("#{bin}/nfnl-iftest --help")
    shell_output("#{bin}/nfnl-iftest --bogus 2>&1", 2)
  end
end
