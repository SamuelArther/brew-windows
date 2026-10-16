class TerraformModuleVersions < Formula
  desc "CLI that checks Terraform code for module updates"
  homepage "https://github.com/keilerkonzept/terraform-module-versions"
  url "https://github.com/keilerkonzept/terraform-module-versions/releases/download/v3.3.13/terraform-module-versions_Windows_x86_64.zip"
  sha256 "03765157e3610884f2abb648ce481aa145ac8437423fd652fb68310fedb74e1a"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/keilerkonzept/terraform-module-versions/archive/refs/tags/v3.3.13.tar.gz"
  end

  def install
    bin.install "terraform-module-versions.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/terraform-module-versions version")

    assert_match "TYPE", shell_output("#{bin}/terraform-module-versions list")
    assert_match "UPDATE?", shell_output("#{bin}/terraform-module-versions check")
  end
end
