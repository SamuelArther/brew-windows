class Bluetoothconnector < Formula
  desc "Connect and disconnect Bluetooth devices"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "BluetoothConnector", "BluetoothConnector"
  end

  test do
    assert_match "BluetoothConnector [--connect", shell_output("#{bin}/BluetoothConnector", 64)
    assert_match "Not paired to device", shell_output("#{bin}/BluetoothConnector --connect 00-00-00-00-00-00", 252)
  end
end
