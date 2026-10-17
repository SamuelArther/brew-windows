class OtelCli < Formula
  desc "Tool for sending events from shell scripts & similar environments"
  homepage "https://github.com/equinix-labs/otel-cli"
  url "https://github.com/equinix-labs/otel-cli/archive/refs/tags/v0.4.5.tar.gz"
  sha256 "331a76783974318a31d9ab06e3f05af488e0ede3cce989f8d1b634450a345536"
  license "Apache-2.0"
  head "https://github.com/equinix-labs/otel-cli.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "e2cfc3498e45fc42ee51863c6c887de9b8bb2767e51fa53779ca0a09775eb7c8"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: :goreleaser)

    generate_completions_from_executable(bin/"otel-cli", shell_parameter_format: :cobra)
  end

  test do
    output = shell_output("#{bin}/otel-cli status")
    assert_equal "otel-cli", JSON.parse(output)["config"]["service_name"]
  end
end
