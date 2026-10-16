class Vibecheck < Formula
  desc "AI-powered git commit assistant written in Go"
  homepage "https://vibe.raashed.xyz"
  url "https://github.com/rshdhere/vibecheck/archive/refs/tags/v1.7.7.tar.gz"
  sha256 "8fef335a930a1c752465e871125d269a72df4f39b6cec65ca6531383faba4f9c"
  license "MIT"
  head "https://github.com/rshdhere/vibecheck.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "6804dfb96896b4d720a252a4ba11f1979574512fa33da7007a49113d69bca9c6"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = "-X github.com/rshdhere/vibecheck/cmd.version=#{version}"
    system "go", "build", *std_go_args(ldflags:)

    generate_completions_from_executable(bin/"vibecheck", shell_parameter_format: :cobra)
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/vibecheck --version")
    assert_match "vibecheck self-test OK", shell_output("#{bin}/vibecheck doctor")
  end
end
