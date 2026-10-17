class Terrascan < Formula
  desc "Detect compliance and security violations across Infrastructure as Code"
  homepage "https://runterrascan.io/"
  url "https://github.com/tenable/terrascan/archive/refs/tags/v1.19.9.tar.gz"
  sha256 "13c120a63d7024ca8c54422e047424e318622625336ed77b2c1a36ef5fb1441c"
  license "Apache-2.0"
  head "https://github.com/tenable/terrascan.git", branch: "master"

  deprecate! date: "2026-05-23", because: :repo_archived

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "c4e97e81b2bf2138a5f924f9dbffac32ea89647eb7fbcc1d558b3ea7fe8e315e"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = "-X google.golang.org/protobuf/reflect/protoregistry.conflictPolicy=ignore"
    system "go", "build", *std_go_args(ldflags:), "./cmd/terrascan"

    generate_completions_from_executable(bin/"terrascan", shell_parameter_format: :cobra)
  end

  test do
    (testpath/"ami.tf").write <<~HCL
      resource "aws_ami" "example" {
        name                = "terraform-example"
        virtualization_type = "hvm"
        root_device_name    = "/dev/xvda"

        ebs_block_device {
          device_name = "/dev/xvda"
          snapshot_id = "snap-xxxxxxxx"
          volume_size = 8
        }
      }
    HCL

    expected = <<~EOS
      \tViolated Policies   :\t0
      \tLow                 :\t0
      \tMedium              :\t0
      \tHigh                :\t0
    EOS

    output = shell_output("#{bin}/terrascan scan -f #{testpath}/ami.tf -t aws")
    assert_match expected, output
    assert_match(/Policies Validated\s+:\s+\d+/, output)
  end
end
