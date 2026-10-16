class DockerDebug < Formula
  desc "Use new container attach on already container go on debug"
  homepage "https://github.com/zeromake/docker-debug"
  url "https://github.com/zeromake/docker-debug/archive/refs/tags/v0.7.11.tar.gz"
  sha256 "f872f649db05f3670650dd7aa3507b0658eb29557d0d2685658ab581b2919101"
  license "MIT"
  head "https://github.com/zeromake/docker-debug.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "e9139a276612e4b7beff299779a15eafe4453bb83b7857f55cd52c31c5f44ff5"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ENV["CGO_ENABLED"] = OS.mac? ? "1" : "0"
    ldflags = %W[
      -X github.com/zeromake/docker-debug/version.Version=#{version}
      -X github.com/zeromake/docker-debug/version.GitCommit=#{tap.user}
      -X github.com/zeromake/docker-debug/version.BuildTime=#{time.iso8601}
      -X github.com/zeromake/docker-debug/version.PlatformName=#{tap.user}
    ]
    system "go", "build", *std_go_args(ldflags:), "./cmd/docker-debug"

    generate_completions_from_executable(bin/"docker-debug", shell_parameter_format: :cobra)
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/docker-debug info")

    system bin/"docker-debug", "init"
    assert_match 'mount_dir = "/mnt/container"', (testpath/".docker-debug/config.toml").read

    assert_match '"TLS": false', shell_output("#{bin}/docker-debug config")
  end
end
