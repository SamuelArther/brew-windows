class Talosctl < Formula
  desc "CLI for out-of-band management of Kubernetes nodes created by Talos"
  homepage "https://www.talos.dev/"
  url "https://github.com/siderolabs/talos/archive/refs/tags/v1.14.7.tar.gz"
  sha256 "e27a9541d19d6276c6beb82f7af9cf8638fd40105da04ea8dbbf15a707ad9a35"
  license "MPL-2.0"
  head "https://github.com/siderolabs/talos.git", branch: "main"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "c45a3556f8ffd81d15984f3bdb7a4db13a4fdd3110ddaca649c97bde6c95f2e2"
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
