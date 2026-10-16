class Gomodifytags < Formula
  desc "Go tool to modify struct field tags"
  homepage "https://github.com/fatih/gomodifytags"
  url "https://github.com/fatih/gomodifytags/archive/refs/tags/v1.17.0.tar.gz"
  sha256 "a490786d80c962dc946b8f9804ffec596c1087dbf91b122b9b5903c03b6da6b9"
  license "BSD-3-Clause"
  head "https://github.com/fatih/gomodifytags.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "1661eaa20a3f12939acc34a87528f3abe57cfdcbc59172c8a61ca2223782ae2d"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args
  end

  test do
    (testpath/"test.go").write <<~GO
      package main

      type Server struct {
      	Name        string
      	Port        int
      	EnableLogs  bool
      	BaseDomain  string
      	Credentials struct {
      		Username string
      		Password string
      	}
      }
    GO
    expected = <<~EOS
      package main

      type Server struct {
      	Name        string `json:"name"`
      	Port        int    `json:"port"`
      	EnableLogs  bool   `json:"enable_logs"`
      	BaseDomain  string `json:"base_domain"`
      	Credentials struct {
      		Username string `json:"username"`
      		Password string `json:"password"`
      	} `json:"credentials"`
      }

    EOS
    assert_equal expected, shell_output("#{bin}/gomodifytags -file test.go -struct Server -add-tags json")
  end
end
