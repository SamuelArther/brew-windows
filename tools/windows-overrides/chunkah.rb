class Chunkah < Formula
  desc "OCI building tool for content-based layers"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "chunkah", "chunkah"
  end

  test do
    (testpath/"rootfs/bin").mkpath
    (testpath/"rootfs/etc").mkpath
    (testpath/"rootfs/lib/apk/db").mkpath
    (testpath/"rootfs/bin/hello").write "#!/bin/sh\necho hello\n"
    (testpath/"rootfs/etc/motd").write "Welcome\n"
    (testpath/"rootfs/lib/apk/db/installed").write "P:hello\nV:1.0\nF:bin\nR:hello\n\n"
    system bin/"chunkah", "build", "--rootfs", "rootfs", "--output", "output.tar", "--tag", "brew/test:1"
    assert_path_exists testpath/"output.tar"
    index = shell_output("tar -xOf output.tar index.json")
    assert_match "application/vnd.oci.image.manifest.v1+json", index
    assert_match "brew/test:1", index
    assert_match "blobs/sha256/", shell_output("tar -tf output.tar")
  end
end
