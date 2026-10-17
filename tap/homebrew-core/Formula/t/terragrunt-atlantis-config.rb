class TerragruntAtlantisConfig < Formula
  desc "Generate Atlantis config for Terragrunt projects"
  homepage "https://transcend.io/blog/why-we-use-terragrunt"
  url "https://github.com/transcend-io/terragrunt-atlantis-config/archive/refs/tags/v1.21.1.tar.gz"
  sha256 "eefc48f2bedc11c154c6c7e088bb10316d811b2d6b851b11d37d80f18a28e517"
  license "MIT"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "cdfb74ff88d7efdc583c1a4c8ecd239c499b7f2f87a1beffd5fd3fbafb255090"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args
    generate_completions_from_executable(bin/"terragrunt-atlantis-config", shell_parameter_format: :cobra)
  end

  test do
    output = shell_output("#{bin}/terragrunt-atlantis-config generate --root #{testpath} 2>&1")
    assert_match "Could not find an old config file. Starting from scratch", output

    assert_match version.to_s, shell_output("#{bin}/terragrunt-atlantis-config version")
  end
end
