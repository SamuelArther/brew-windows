class S3Backer < Formula
  desc "Block device stored in Amazon S3, attached as an iSCSI disk"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "s3backer", "s3backer"
  end

  def caveats
    <<~EOS
      s3backer keeps a block device in S3 (one object per block, zero blocks not stored) and serves
      it as an iSCSI target that Windows' built-in iSCSI initiator attaches (administrator): it
      appears as a new disk to initialize and format the first time. Ctrl+C detaches it.
      --test uses a local directory instead of a bucket. --compress and --encrypt aren't available.
    EOS
  end

  test do
    assert_match "2.1.6", shell_output("#{bin}/s3backer --version 2>&1")
    assert_match "no S3 bucket specified", shell_output("#{bin}/s3backer 2>&1", 1)
    (testpath/"bucket").mkpath
    assert_match "auto-detect size", shell_output("#{bin}/s3backer --test #{testpath}/bucket --listBlocks 2>&1", 1)
    assert_equal "", shell_output("#{bin}/s3backer --test --size=1M #{testpath}/bucket --listBlocks")
    assert_match "erased 0 objects", shell_output("#{bin}/s3backer --test --force --erase #{testpath}/bucket 2>&1")
    reset = shell_output("#{bin}/s3backer --test --reset-mounted-flag #{testpath}/bucket 2>&1")
    assert_match "reset mounted flag", reset
  end
end
