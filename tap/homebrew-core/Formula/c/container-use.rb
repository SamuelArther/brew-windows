class ContainerUse < Formula
  desc "Dev envs for coding agents. Run multiple agents safely with your stack"
  homepage "https://container-use.com/introduction"
  url "https://github.com/dagger/container-use/archive/refs/tags/v0.4.2.tar.gz"
  sha256 "951105f0b4a9bfd9f52e7bb3a2d245e800df4b8449704cd34001833ee888a02d"
  license "Apache-2.0"
  head "https://github.com/dagger/container-use.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "e245cf2f1376acf174333725461913e06afbe2324527efe6b5ebad3bf5c48d5b"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: :goreleaser), "./cmd/container-use"

    generate_completions_from_executable(bin/"container-use", shell_parameter_format: :cobra)
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/container-use version 2>&1")

    system "git", "init"
    assert_match "No environment variables configured", shell_output("#{bin}/container-use config env list")
  end
end
