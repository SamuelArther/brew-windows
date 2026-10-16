class DeviceMapper < Formula
  desc "Volume-to-disk mapping tools for Windows volumes (dmsetup)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "dmsetup", "dmsetup"
  end

  test do
    assert_match "Library version", shell_output("#{bin}/dmsetup version")
    assert_match "linear", shell_output("#{bin}/dmsetup targets")
    # The system volume maps onto a physical disk like a linear target.
    assert_match(/^0 \d+ linear PhysicalDrive\d+ \d+$/, shell_output("#{bin}/dmsetup table C:"))
    assert_match "dependencies", shell_output("#{bin}/dmsetup deps C:")
    assert_match "not found", shell_output("#{bin}/dmsetup info no-such-device 2>&1", 1)
    assert_match "volume manager", shell_output("#{bin}/dmsetup create test 2>&1", 1)
  end
end
