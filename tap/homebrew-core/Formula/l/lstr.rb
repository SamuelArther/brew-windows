class Lstr < Formula
  desc "Fast, minimalist directory tree viewer"
  homepage "https://github.com/bgreenwell/lstr"
  url "https://github.com/bgreenwell/lstr/releases/download/v0.4.0/lstr-x86_64-pc-windows-msvc.zip"
  sha256 "92be741d5868abb3ae5a3cff109a8679b674c65fa53186cf044968e026c13282"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/bgreenwell/lstr/archive/refs/tags/v0.4.0.tar.gz"
  end

  def install
    bin.install "lstr.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/lstr --version")

    (testpath/"test_dir/file1.txt").write "Hello, World!"
    assert_match "file1.txt", shell_output("#{bin}/lstr test_dir")
  end
end
