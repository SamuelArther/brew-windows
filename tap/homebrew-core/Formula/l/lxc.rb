class Lxc < Formula
  desc "CLI client for interacting with LXD"
  homepage "https://ubuntu.com/lxd"
  url "https://github.com/canonical/lxd/archive/refs/tags/lxd-6.10.1.tar.gz"
  sha256 "687495b7b989da5f29ab009f7e728462f845ebf65696d30c5a6a0d646ce3fb58"
  license "AGPL-3.0-only"
  head "https://github.com/canonical/lxd.git", branch: "main"

  livecheck do
    url :stable
    regex(/^lxd-(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "b5e19510ae97e155b2182a889430ac09bbdc31b27ffe7524fe7935763a3c5034"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args, "./lxc"

    generate_completions_from_executable(bin/"lxc", shell_parameter_format: :cobra)
  end

  test do
    output = JSON.parse(shell_output("#{bin}/lxc remote list --format json"))
    assert_equal "https://cloud-images.ubuntu.com/releases/", output["ubuntu"]["Addr"]

    assert_match version.to_s, shell_output("#{bin}/lxc --version")
  end
end
