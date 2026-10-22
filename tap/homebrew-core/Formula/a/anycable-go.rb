class AnycableGo < Formula
  desc "WebSocket server with action cable protocol"
  homepage "https://anycable.io"
  url "https://github.com/anycable/anycable/archive/refs/tags/v1.6.20.tar.gz"
  sha256 "d03981135c87401e298b5e79bcad0571e2887db9b946d22fd44f13c24b56fc05"
  license "MIT"
  head "https://github.com/anycable/anycable.git", branch: "main"

  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "7311c0f750b2e9cd8c468e18d13b70f7dbdd410500ea7fabdd8be29088138db3"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = if build.head?
      "-X github.com/anycable/anycable/utils.sha=#{version.commit}"
    else
      "-X github.com/anycable/anycable/utils.version=#{version}"
    end

    system "go", "build", *std_go_args(ldflags:), "./cmd/anycable-go"
  end

  test do
    port = free_port
    pid = spawn bin/"anycable-go", "--port=#{port}"
    sleep 1
    output = shell_output("curl -sI http://localhost:#{port}/health")
    assert_match(/200 OK/m, output)
  ensure
    Process.kill("HUP", pid)
  end
end
