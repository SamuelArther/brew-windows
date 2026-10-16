class LibcapNg < Formula
  desc "Capability reporting tools for Windows token privileges"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "pscap", "pscap"
    windows_exe "libexec/wintools.exe", "netcap", "netcap"
    windows_exe "libexec/wintools.exe", "filecap", "filecap"
    windows_exe "libexec/wintools.exe", "captest", "captest"
  end

  def caveats
    <<~EOS
      Capabilities are Windows token privileges; Windows files carry none, so filecap reports
      nothing. There is no libcap-ng C library or Python module on Windows.
    EOS
  end

  test do
    assert_match "capabilities", shell_output("#{bin}/pscap -a")
    assert_match "Current capabilities", shell_output("#{bin}/captest")
    assert_match "Child capabilities: none", shell_output("#{bin}/captest --drop-all")
    assert_equal "", shell_output("#{bin}/filecap #{testpath}")
    shell_output("#{bin}/netcap")
  end
end
