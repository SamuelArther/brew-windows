class Tfmigrate < Formula
  desc "Terraform/OpenTofu state migration tool for GitOps"
  homepage "https://github.com/minamijoyo/tfmigrate"
  url "https://github.com/minamijoyo/tfmigrate/archive/refs/tags/v0.4.5.tar.gz"
  sha256 "306c8d14bfbe86a532c85fffca2c4ea2cca6412bcbc4016680f81643d0d630e9"
  license "MIT"
  head "https://github.com/minamijoyo/tfmigrate.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "e292fdad6fa643ebbae6b66a3ab85e6c353e9ad9f941dd1c4af19c7d3b3de882"
  end

  depends_on "go" => :build
  depends_on "opentofu" => :test

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args
  end

  test do
    ENV["TFMIGRATE_EXEC_PATH"] = "tofu"

    (testpath/"tfmigrate.hcl").write <<~HCL
      migration "state" "brew" {
        actions = [
          "mv aws_security_group.foo aws_security_group.baz",
        ]
      }
    HCL
    output = shell_output("#{bin}/tfmigrate plan tfmigrate.hcl 2>&1", 1)
    assert_match "[migrator@.] compute a new state", output
    assert_match "No state file was found", output

    assert_match version.to_s, shell_output("#{bin}/tfmigrate --version")
  end
end
