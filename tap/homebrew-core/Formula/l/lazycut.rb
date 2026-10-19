class Lazycut < Formula
  desc "Terminal-based video trimming TUI"
  homepage "https://github.com/ozemin/lazycut"
  url "https://github.com/ozemin/lazycut/archive/refs/tags/v0.4.0.tar.gz"
  sha256 "ca435f59c1d16e71b9917f36f731887eba001d8a06813f02d1785306e92578c5"
  license "MIT"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "bcfb9cc84bd1ce1890a6f8e5a1711b0d758517d255477e079ff16be36d83847b"
  end

  depends_on "go" => :build
  depends_on "chafa"
  depends_on "ffmpeg"

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: "-X main.version=#{version}")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/lazycut --version")

    system "ffmpeg", "-f", "lavfi", "-i", "testsrc2=duration=3:size=320x240:rate=25",
           "-c:v", "libx264", "-t", "3", testpath/"test.mp4"
    output = shell_output("#{bin}/lazycut probe #{testpath}/test.mp4")
    assert_match "Duration:", output
    assert_match "Resolution:", output

    system bin/"lazycut", "trim", "--in", "00:00:00", "--out", "00:00:02",
           "-o", testpath/"trimmed.mp4", testpath/"test.mp4"
    assert_path_exists testpath/"trimmed.mp4"
  end
end
