class Xmount < Formula
  desc "Convert between raw, VHD and VMDK disk images"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "xmount", "xmount"
  end

  test do
    assert_match "xmount", shell_output("#{bin}/xmount --version")
    (testpath/"disk.dd").write "a" * 1_048_576
    (testpath/"mnt").mkpath
    system bin/"xmount", "--in", "raw", testpath/"disk.dd", "--out", "vhd", testpath/"mnt"
    assert_path_exists testpath/"mnt/disk.vhd"
    assert_match "Size: 1048576 bytes", (testpath/"mnt/disk.info").read
    system bin/"xmount", "-u", testpath/"mnt"
    refute_path_exists testpath/"mnt/disk.vhd"
  end
end
