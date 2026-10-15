class ThePlatinumSearcher < Formula
  desc "Multi-platform code-search similar to ack and ag"
  homepage "https://github.com/monochromegane/the_platinum_searcher"
  url "https://github.com/monochromegane/the_platinum_searcher/releases/download/v2.2.0/pt_windows_amd64.zip"
  sha256 "7fa5aab0462a63cc1a746af3f07f1d03bf6521e23f8b2d149fc5af7b4e00596c"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/monochromegane/the_platinum_searcher/archive/refs/tags/v2.2.0.tar.gz"
  end

  def install
    bin.install "pt.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/pt --version 2>&1")
  end
end
