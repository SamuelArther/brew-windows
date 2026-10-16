class Pug < Formula
  desc "Drive terraform at terminal velocity"
  homepage "https://github.com/leg100/pug"
  url "https://github.com/leg100/pug/releases/download/v0.6.5/pug_0.6.5_windows_amd64.zip"
  sha256 "209ed388582a712726ff434f80efb2a435765d83610da7cd2346d3107fb6a0c7"
  license "MPL-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/leg100/pug/archive/refs/tags/v0.6.5.tar.gz"
  end

  def install
    bin.install "pug.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/pug --version 2>&1")
  end
end
