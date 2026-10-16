class Typtea < Formula
  desc "Minimal terminal-based typing speed tester"
  homepage "https://github.com/ashish0kumar/typtea"
  url "https://github.com/ashish0kumar/typtea/releases/download/v0.1.6/typtea_windows_x86_64.zip"
  sha256 "6b3d37400a1d9c55c621f524f01db496fb5602488b8fead93da17507a88feec2"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/ashish0kumar/typtea/archive/refs/tags/v0.1.6.tar.gz"
  end

  def install
    bin.install "typtea.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/typtea version")

    assert_match "python", shell_output("#{bin}/typtea start --list-langs 2>&1")
  end
end
