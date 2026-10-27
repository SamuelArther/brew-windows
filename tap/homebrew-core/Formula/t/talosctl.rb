class Talosctl < Formula
  desc "CLI for out-of-band management of Kubernetes nodes created by Talos"
  homepage "https://www.talos.dev/"
  url "https://github.com/siderolabs/talos/archive/refs/tags/v1.14.5.tar.gz"
  sha256 "cb44b8cfcb73d300f8d50d94f075d68cb072a8ebff6d4d4b6ef5c4355a57dfc0"
  license "MPL-2.0"
  head "https://github.com/siderolabs/talos.git", branch: "main"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "d1d0431aeb5307331a842bc3d981a73a5fa952f725f5fa23d8f4fe6b1930d2db"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = %W[
      -X github.com/siderolabs/talos/pkg/machinery/version.Tag=#{version}
      -X github.com/siderolabs/talos/pkg/machinery/version.Built=#{time.iso8601}

    ]
    system "go", "build", *std_go_args(ldflags:), "./cmd/talosctl"

    generate_completions_from_executable(bin/"talosctl", shell_parameter_format: :cobra)
  end

  test do
    # version check also failed with `failed to determine endpoints` for server config
    assert_match version.to_s, shell_output("#{bin}/talosctl version 2>&1", 1)

    output = shell_output("#{bin}/talosctl list 2>&1", 1)
    assert_match "failed to determine endpoints", output
  end
end
