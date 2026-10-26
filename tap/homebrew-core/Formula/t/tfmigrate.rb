class Tfmigrate < Formula
  desc "Terraform/OpenTofu state migration tool for GitOps"
  homepage "https://github.com/minamijoyo/tfmigrate"
  url "https://github.com/minamijoyo/tfmigrate/archive/refs/tags/v0.4.6.tar.gz"
  sha256 "877b778ef2512a24623c3b96b00c883378b281bbcb2db6f2d106463883f9a145"
  license "MIT"
  head "https://github.com/minamijoyo/tfmigrate.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "d0c54ccbf312a118f55a2076c5db8b4d522af3ba232ed9a1354ae4ba11f78f9d"
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
