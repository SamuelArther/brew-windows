class Xmount < Formula
  desc "Convert between raw, VHD and VMDK disk images"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

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
