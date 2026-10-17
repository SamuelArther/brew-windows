class Kubehound < Formula
  desc "Tool for building Kubernetes attack paths"
  homepage "https://kubehound.io"
  url "https://github.com/DataDog/KubeHound/archive/refs/tags/v1.6.7.tar.gz"
  sha256 "b753e20ef6200e1bb0e26e0afba9bf9e76f5ea7a2d823bf90dc88b60f4ebd31d"
  license "Apache-2.0"
  head "https://github.com/DataDog/KubeHound.git", branch: "main"

  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "705db721ab84d720a06257072836e2a9d00a0bf8242561f234a23113ca50fdfe"
  end

  depends_on "go" => [:build, :test]

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    goos = Utils.safe_popen_read("#{formula_opt_bin("go")}/go", "env", "GOOS").chomp
    goarch = Utils.safe_popen_read("#{formula_opt_bin("go")}/go", "env", "GOARCH").chomp

    ldflags = %W[
      -X github.com/DataDog/KubeHound/pkg/config.BuildVersion=v#{version}
      -X github.com/DataDog/KubeHound/pkg/config.BuildBranch=main
      -X github.com/DataDog/KubeHound/pkg/config.BuildOs=#{goos}
      -X github.com/DataDog/KubeHound/pkg/config.BuildArch=#{goarch}
    ]
    system "go", "build", *std_go_args(ldflags:, tags: "no_backend"), "./cmd/kubehound/"

    generate_completions_from_executable(bin/"kubehound", shell_parameter_format: :cobra)
  end

  test do
    assert_match "kubehound version: v#{version}", shell_output("#{bin}/kubehound version")

    ENV["DOCKER_HOST"] = "unix://#{testpath}/invalid.sock"
    error_message = "error starting the kubehound stack"
    assert_match error_message, shell_output("#{bin}/kubehound backend up 2>&1", 1)
  end
end
