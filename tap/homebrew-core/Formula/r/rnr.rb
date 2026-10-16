class Rnr < Formula
  desc "Command-line tool to batch rename files and directories"
  homepage "https://github.com/ismaelgv/rnr"
  url "https://github.com/ismaelgv/rnr/releases/download/v0.5.1/rnr-v0.5.1-x86_64-pc-windows-gnu.zip"
  sha256 "c69b4d3cff980589a6d80aea6a3bb3e1f8b1e1c469c27b7578ba4dbba8f32916"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/ismaelgv/rnr/archive/refs/tags/v0.5.1.tar.gz"
  end

  def install
    bin.install "rnr.exe"
  end

  test do
    touch "foo.doc"
    mkdir "one"
    touch "one/foo.doc"

    system bin/"rnr", "regex", "-f", "doc", "txt", "foo.doc", "one/foo.doc"
    refute_path_exists testpath/"foo.doc"
    assert_path_exists testpath/"foo.txt"

    assert_match version.to_s, shell_output("#{bin}/rnr --version")
  end
end
