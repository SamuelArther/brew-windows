class Bombardier < Formula
  desc "Cross-platform HTTP benchmarking tool"
  homepage "https://github.com/codesenberg/bombardier"
  url "https://github.com/codesenberg/bombardier/archive/refs/tags/v2.0.2.tar.gz"
  sha256 "472b14b1c3be26a5f6254f6b7c24f86c9b756544baa5ca28cbfad06aacf7f4ac"
  license "MIT"
  head "https://github.com/codesenberg/bombardier.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "f4fea67ece323f0c7888c08d20793bd7bbd25e447457a4ad8886a0e326c7fdba"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: "-X main.version=#{version}")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/bombardier --version 2>&1")

    url = "https://example.com"
    output = shell_output("#{bin}/bombardier -c 1 -n 1 #{url}")
    assert_match "Bombarding #{url} with 1 request(s) using 1 connection(s)", output
  end
end
