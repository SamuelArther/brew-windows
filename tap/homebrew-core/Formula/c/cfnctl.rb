class Cfnctl < Formula
  desc "Brings the Terraform cli experience to AWS Cloudformation"
  homepage "https://github.com/rogerwelin/cfnctl"
  url "https://github.com/rogerwelin/cfnctl/releases/download/v0.1.1/cfnctl_Windows_x86_64.zip"
  sha256 "2ea1e3b1d8f143883b5db919dedb0c6b972f52c53f2d4887ac652c7ad646af4b"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/rogerwelin/cfnctl/archive/refs/tags/v0.1.1.tar.gz"
  end

  def install
    bin.install "cfnctl.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cfnctl version")

    ENV["AWS_DEFAULT_REGION"] = "us-east-1"
    ENV["AWS_ACCESS_KEY_ID"]     = "dummy"
    ENV["AWS_SECRET_ACCESS_KEY"] = "dummy"

    (testpath/"test.yaml").write <<~YAML
      Resources:
        Bucket:
          Type: AWS::S3::Bucket
    YAML

    output = shell_output("#{bin}/cfnctl validate --template-file test.yaml 2>&1")
    assert_match "ValidateTemplate, https response error StatusCode: 403", output
  end
end
