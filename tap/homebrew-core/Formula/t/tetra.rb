class Tetra < Formula
  desc "Tetragon CLI to observe, manage and troubleshoot Tetragon instances"
  homepage "https://tetragon.io/"
  url "https://github.com/cilium/tetragon/archive/refs/tags/v1.8.0.tar.gz"
  sha256 "49538903f16bd08bfa24ff4b1bfb16c0edf93eebd730e072425200a4b21faed1"
  license "Apache-2.0"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "7d07cc915cce2ed9a53b08f91b78cdabf3defd78cb267e249bb66d2e336c3772"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = "-X github.com/cilium/tetragon/pkg/version.Version=#{version}"
    system "go", "build", *std_go_args(ldflags:, output: bin/"tetra"), "./cmd/tetra"

    generate_completions_from_executable(bin/"tetra", shell_parameter_format: :cobra)
  end

  test do
    assert_match "CLI version: #{version}", shell_output("#{bin}/tetra version --build")
    event = '{"process_exec":{"process":{"binary":"/usr/bin/foo","arguments":"bar"}}}'
    assert_match "/usr/bin/foo bar", pipe_output("#{bin}/tetra getevents -o compact", event)
  end
end
