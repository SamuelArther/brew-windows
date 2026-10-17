class Cyphernetes < Formula
  desc "Kubernetes Query Language"
  homepage "https://cyphernet.es"
  url "https://github.com/AvitalTamir/cyphernetes/archive/refs/tags/v0.18.2.tar.gz"
  sha256 "98c48dbc4263854c74c9216274c8ec1cb327d9accbf0458f86c2231de8c889e9"
  license "Apache-2.0"
  head "https://github.com/AvitalTamir/cyphernetes.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "f597eba993edf908be1cdc6ff1e06afc77dea988031b0e368bf517dfd7ac5ca5"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
    # downloads and runs `controller-gen` and `kustomize` to generate embedded manifests
    system "make", "operator-manifests"
  end

  def install
    system "go", "build", *std_go_args(ldflags: "-X main.Version=#{version}"), "./cmd/cyphernetes"

    generate_completions_from_executable(bin/"cyphernetes", shell_parameter_format: :cobra)
  end

  test do
    output = shell_output("#{bin}/cyphernetes query 'MATCH (d:Deployment)->(s:Service) RETURN d' 2>&1", 1)
    assert_match("Error creating provider:  failed to create config: invalid configuration", output)

    assert_match version.to_s, shell_output("#{bin}/cyphernetes version")
  end
end
