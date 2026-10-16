class AwsSpiffeWorkloadHelper < Formula
  desc "Helper for providing AWS credentials to workloads using their SPIFFE identity"
  homepage "https://github.com/spiffe/aws-spiffe-workload-helper"
  url "https://github.com/spiffe/aws-spiffe-workload-helper/releases/download/v0.0.6/aws-spiffe-workload-helper_Windows_x86_64.zip"
  sha256 "94bb2806b701e865ae06fdbc92080f271ea481510c24a1a723d4c788a85a0587"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/spiffe/aws-spiffe-workload-helper/archive/refs/tags/v0.0.6.tar.gz"
  end

  def install
    bin.install "aws-spiffe-workload-helper.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/aws-spiffe-workload-helper --version")

    output = shell_output("#{bin}/aws-spiffe-workload-helper jwt-credential-process " \
                          "--audience test-audience --endpoint http://localhost 2>&1", 1)
    assert_match "Error: creating workload api client: workload endpoint socket address is not configured", output
  end
end
