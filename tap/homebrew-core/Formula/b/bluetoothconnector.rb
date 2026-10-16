class Bluetoothconnector < Formula
  desc "Connect and disconnect Bluetooth devices"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

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
