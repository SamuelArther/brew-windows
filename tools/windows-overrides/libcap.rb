class Libcap < Formula
  desc "Capability tools for Windows token privileges"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "capsh", "capsh"
    windows_exe "libexec/wintools.exe", "getpcaps", "getpcaps"
    windows_exe "libexec/wintools.exe", "getcap", "getcap"
    windows_exe "libexec/wintools.exe", "setcap", "setcap"
  end

  def caveats
    <<~EOS
      Capabilities are Windows token privileges (SeDebugPrivilege, SeShutdownPrivilege, ...).
      Windows files cannot carry capabilities, so getcap reports none and setcap refuses.
      There is no libcap C library: programs use AdjustTokenPrivileges and CreateRestrictedToken.
    EOS
  end

  test do
    assert_match "usage", shell_output("#{bin}/getcap 2>&1", 2)
    assert_match "SeChangeNotifyPrivilege", shell_output("#{bin}/capsh --print")
    assert_match "=cap_chown,cap_dac_override", shell_output("#{bin}/capsh --decode=3")
    assert_match "dropped", shell_output("#{bin}/capsh --drop=SeShutdownPrivilege -- -c \"echo dropped\"")
    shell_output("#{bin}/capsh --has-p=SeChangeNotifyPrivilege")
    shell_output("#{bin}/setcap cap_net_raw+ep #{bin}/capsh 2>&1", 1)
  end
end
