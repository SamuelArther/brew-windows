class TerraformInventory < Formula
  desc "Go app which generates a dynamic Ansible inventory from a Terraform state file"
  homepage "https://github.com/adammck/terraform-inventory"
  url "https://github.com/adammck/terraform-inventory/releases/download/v0.10/terraform-inventory_v0.10_windows_amd64.zip"
  sha256 "2cba0132b6fbcbeded528272d4717dd5a5d685c4ff44723c3194a07990487c01"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/adammck/terraform-inventory/archive/refs/tags/v0.10.tar.gz"
  end

  def install
    bin.install "terraform-inventory.exe"
  end

  test do
    example = <<~JSON
      {
          "version": 1,
          "serial": 1,
          "modules": [
              {
                  "path": [
                      "root"
                  ],
                  "outputs": {},
                  "resources": {
                      "aws_instance.example_instance": {
                          "type": "aws_instance",
                          "primary": {
                              "id": "i-12345678",
                              "attributes": {
                                  "public_ip": "1.2.3.4"
                              },
                              "meta": {
                                  "schema_version": "1"
                              }
                          }
                      }
                  }
              }
          ]
      }
    JSON
    (testpath/"example.tfstate").write(example)
    assert_match(/example_instance/, shell_output("#{bin}/terraform-inventory --list example.tfstate"))
  end
end
