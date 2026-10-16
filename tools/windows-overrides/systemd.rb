class Systemd < Formula
  desc "Service manager tools (systemctl, journalctl) for Windows services and the Event Log"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "systemctl", "systemctl"
    windows_exe "libexec/wintools.exe", "journalctl", "journalctl"
    windows_exe "libexec/wintools.exe", "systemd-path", "systemd-path"
    windows_exe "libexec/wintools.exe", "hostnamectl", "hostnamectl"
    windows_exe "libexec/wintools.exe", "timedatectl", "timedatectl"
    windows_exe "libexec/wintools.exe", "loginctl", "loginctl"
  end

  test do
    assert_match "temporary: ", shell_output(bin/"systemd-path")
    assert_match "EventLog.service", shell_output("#{bin}/systemctl list-units --all --no-legend")
    assert_equal "active\n", shell_output("#{bin}/systemctl is-active EventLog")
    assert_match "could not be found", shell_output("#{bin}/systemctl status no-such-unit 2>&1", 4)
    assert_match "Static hostname", shell_output(bin/"hostnamectl")
    assert_match "Time zone", shell_output(bin/"timedatectl")
    system bin/"journalctl", "-n", "1", "--no-pager"
    assert_match "systemd", shell_output("#{bin}/journalctl --version")
  end
end
