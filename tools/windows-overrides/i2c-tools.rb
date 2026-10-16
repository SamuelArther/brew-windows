class I2cTools < Formula
  desc "Heterogeneous set of I2C tools (on Windows' I2C controllers)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "i2cdetect", "i2cdetect"
    windows_exe "libexec/wintools.exe", "i2cget", "i2cget"
    windows_exe "libexec/wintools.exe", "i2cset", "i2cset"
    windows_exe "libexec/wintools.exe", "i2cdump", "i2cdump"
    windows_exe "libexec/wintools.exe", "i2ctransfer", "i2ctransfer"
    windows_exe "libexec/wintools.exe", "eeprog", "eeprog"
    windows_exe "libexec/wintools.exe", "decode-dimms", "decode-dimms"
  end

  def caveats
    <<~EOS
      Windows only shows applications the I2C controllers its firmware exposes (SPB controllers on
      tablets, boards and Windows IoT); i2cdetect -l lists them. decode-dimms reads the SMBIOS memory
      records, since SPD EEPROMs aren't readable without a driver. The py-smbus module isn't included.
    EOS
  end

  test do
    assert_empty shell_output("#{bin}/i2cdetect -l 2>&1").strip
    assert_match "/dev/i2c/0': No such file or directory", shell_output("#{bin}/i2cget -y 0 0x08 2>&1", 1)
    assert_match "Memory Serial Presence Detect Decoder", shell_output("#{bin}/decode-dimms")
    assert_match "Usage: i2ctransfer", shell_output("#{bin}/i2ctransfer -h")
    assert_match "Usage: i2cset", shell_output("#{bin}/i2cset 2>&1", 1)
  end
end
