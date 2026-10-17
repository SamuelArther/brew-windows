class Netfetch < Formula
  desc "K8s tool to scan clusters for network policies and unprotected workloads"
  homepage "https://github.com/deggja/netfetch"
  url "https://github.com/deggja/netfetch/archive/refs/tags/v0.5.4.tar.gz"
  sha256 "6029d93da6633a626d6920944825c76b5552e4ad5175101f661281e30b36b1cf"
  license "MIT"
  head "https://github.com/deggja/netfetch.git", branch: "main"

  livecheck do
    url :stable
    regex(/^v?(0(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "30a76ef3707540eabab4a3a9b748ae226bb454d4e51b7753bb44c43d49a2f371"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = "-X github.com/deggja/netfetch/backend/cmd.Version=#{version}"
    system "go", "build", *std_go_args(ldflags:), "./backend"

    generate_completions_from_executable(bin/"netfetch", shell_parameter_format: :cobra)
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/netfetch version")

    # Windows prints the path with backslashes and words ENOENT "The system cannot find the path specified".
    missing = OS.windows? ? ".kube\\config: The system cannot find the path specified" : ".kube/config: no such file or directory"
    assert_match missing, shell_output("#{bin}/netfetch scan")
  end
end
