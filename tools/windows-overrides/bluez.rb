class Bluez < Formula
  desc "BlueZ's Bluetooth tools (bluetoothctl, hciconfig, hcitool...) for Windows"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "bluetoothctl", "bluetoothctl"
    windows_exe "libexec/wintools.exe", "hciconfig", "hciconfig"
    windows_exe "libexec/wintools.exe", "hcitool", "hcitool"
    windows_exe "libexec/wintools.exe", "rfcomm", "rfcomm"
    windows_exe "libexec/wintools.exe", "sdptool", "sdptool"
    windows_exe "libexec/wintools.exe", "btmon", "btmon"
    windows_exe "libexec/wintools.exe", "bluemoon", "bluemoon"
  end

  test do
    assert_match "5.87", shell_output("#{bin}/bluetoothctl --version")
    assert_match "Failed to open HCI user channel", shell_output("#{bin}/bluemoon 2>&1", 1)
    assert_match "Failed to open channel", shell_output("#{bin}/btmon 2>&1", 1)
    assert_match "Devices:", shell_output("#{bin}/hcitool dev")
    system bin/"rfcomm"
  end
end
