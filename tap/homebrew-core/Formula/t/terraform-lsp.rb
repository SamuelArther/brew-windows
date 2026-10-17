class TerraformLsp < Formula
  desc "Language Server Protocol for Terraform"
  homepage "https://github.com/juliosueiras/terraform-lsp"
  url "https://github.com/juliosueiras/terraform-lsp.git",
      tag:      "v0.0.12",
      revision: "b0a5e4c435a054577e4c01489c1eef7216de4e45"
  license "MIT"
  head "https://github.com/juliosueiras/terraform-lsp.git", branch: "master"

  deprecate! date: "2026-08-23", because: :unmaintained
  disable! date: "2027-08-23", because: :unmaintained

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "c4f8f44bff68e5c1da094325e1d4d433c7bbe57e0ebd23afff62a02341aa9914"
  end

  depends_on "go" => :build

  # Failing on Intel macOS with Go 1.27 due to outdated golang.org/x/sys dependency
  on_macos do
    depends_on arch: :arm64
  end

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = %W[
      -X main.Version=#{version}
      -X main.GitCommit=#{Utils.git_head}
      -X main.Date=#{time.iso8601}
    ]

    system "go", "build", *std_go_args(ldflags:)
  end

  test do
    port = free_port
    pid = spawn bin/"terraform-lsp", "serve", "-tcp", "-port", port.to_s
    begin
      sleep 2
      tcp_socket = TCPSocket.new("localhost", port)
      tcp_socket.puts <<~EOF
        Content-Length: 59

        {"jsonrpc":"2.0","method":"initialize","params":{},"id":1}
      EOF
      assert_match "Content-Length:", tcp_socket.gets("\n")
    ensure
      Process.kill("TERM", pid)
      Process.wait(pid)
    end

    assert_match version.to_s, shell_output("#{bin}/terraform-lsp serve -version")
  end
end
