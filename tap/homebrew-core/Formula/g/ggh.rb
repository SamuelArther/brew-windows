class Ggh < Formula
  desc "Recall your SSH sessions"
  homepage "https://github.com/byawitz/ggh"
  url "https://github.com/byawitz/ggh/releases/download/v0.1.5/ggh_windows_x86_64.exe"
  sha256 "4142f4ffd1a32e6cd63d5bd8eedc538c3c644914287d535c37cd6a10d3f4b5ed"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/byawitz/ggh/archive/refs/tags/v0.1.5.tar.gz"
  end

  def install
    bin.install "ggh_windows_x86_64.exe" => "ggh.exe"
  end

  test do
    assert_match "No history found.", shell_output(bin/"ggh").chomp
    assert_match "No config found.", shell_output("#{bin}/ggh -").chomp
  end
end
