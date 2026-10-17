class Orgalorg < Formula
  desc "Parallel SSH commands executioner and file synchronization tool"
  homepage "https://github.com/reconquest/orgalorg"
  url "https://github.com/reconquest/orgalorg/archive/refs/tags/1.3.1.tar.gz"
  sha256 "b9292ac6af1c492c82e4c77a707a026ad9674139f02d3fa25b797f65e3d69a2c"
  license "MIT"
  head "https://github.com/reconquest/orgalorg.git", branch: "master"

  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "f25aabf4f47cfeac428be92626eca58d8bec40cc75a152810d4c4a736a3ec567"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", "-mod=mod", *std_go_args(ldflags: "-X main.version=#{version}")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/orgalorg --version")
    assert_match "orgalorg - files synchronization on many hosts.", shell_output("#{bin}/orgalorg --help")

    ENV.delete "SSH_AUTH_SOCK"

    port = free_port
    output = shell_output("#{bin}/orgalorg -u tester --key '' --host=127.0.0.1:#{port} -C uptime 2>&1", 1)
    assert_match "connecting to cluster failed", output
    # Windows words a refused connection "No connection could be made because the target machine actively refused it".
    assert_match "dial tcp 127.0.0.1:#{port}: #{OS.windows? ? "connectex: No connection could be made" : "connect: connection refused"}", output
    assert_match "can't connect to address: [tester@127.0.0.1:#{port}]", output
  end
end
