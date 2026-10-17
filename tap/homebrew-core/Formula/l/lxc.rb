class Lxc < Formula
  desc "CLI client for interacting with LXD"
  homepage "https://ubuntu.com/lxd"
  url "https://github.com/canonical/lxd/releases/download/lxd-6.9/lxd-6.9.tar.gz"
  sha256 "c13dff67aa400d3cc4ef3a20344a500fa691263132ad9de843c5485caffe47dd"
  license "AGPL-3.0-only"
  head "https://github.com/canonical/lxd.git", branch: "main"

  livecheck do
    url :stable
    regex(/^lxd-(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "b5da07d98d7755903be95ecf473b949b171068c6ceee6561a312ecdc0c274be8"
  end

  depends_on "go" => :build

  deny_network_access!

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
