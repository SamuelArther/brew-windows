class Logdy < Formula
  desc "Web based real-time log viewer"
  homepage "https://logdy.dev"
  url "https://github.com/logdyhq/logdy-core/archive/refs/tags/v0.17.1.tar.gz"
  sha256 "bd5db124e736e42d3671697787a26b354e0be6e787a95e69c054ad873058fcec"
  license "Apache-2.0"
  head "https://github.com/logdyhq/logdy-core.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "85df8e48aafdebf3eb391ebb76524ea0fc5169f498733f794605af8f45269f8d"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: "-X main.Version=#{version}")

    generate_completions_from_executable(bin/"logdy", shell_parameter_format: :cobra)
  end

  test do
    port = free_port
    r, _, pid = PTY.spawn("#{bin}/logdy stdin --port=#{port}")
    assert_match "Listen to stdin (from pipe)", r.readline
  ensure
    Process.kill("TERM", pid)
  end
end
