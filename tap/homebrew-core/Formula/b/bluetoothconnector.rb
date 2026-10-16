class Bluetoothconnector < Formula
  desc "Connect and disconnect Bluetooth devices"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

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
