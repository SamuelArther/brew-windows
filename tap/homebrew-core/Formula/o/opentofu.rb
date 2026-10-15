class Opentofu < Formula
  desc "Drop-in replacement for Terraform. Infrastructure as Code Tool"
  homepage "https://opentofu.org/"
  version "1.13.1"
  url "https://github.com/opentofu/opentofu/releases/download/v1.13.1/tofu_1.13.1_windows_amd64.zip"
  sha256 "5b653d1d95719eeec4ccf55f0e2102081b6f589d788197f1cabeb89cbd4078a7"
  license "MPL-2.0"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/tofu.exe", "tofu"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tofu version")

    (testpath/"minimal.tf").write <<~TF
      output "hello" {
        value = "world"
      }
    TF
    system bin/"tofu", "init"
  end
end
