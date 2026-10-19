class GoAT125 < Formula
  desc "Open source programming language to build simple/reliable/efficient software"
  homepage "https://go.dev/"
  url "https://go.dev/dl/go1.25.14.src.tar.gz"
  mirror "https://fossies.org/linux/misc/go1.25.14.src.tar.gz"
  sha256 "9e83f44f5fc297378861b4e16cc6aa114be8add7993fb3ceb2c512380aa4d582"
  license "BSD-3-Clause"
  compatibility_version 3

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "683b52a4561fd2ebde4924558f51440b58e2c3bde503b5b7d466e58dc6d39c3a"
  end

  keg_only :versioned_formula

  # EOL with Go 1.27 release (2026-08-19)
  # Ref: https://go.dev/doc/devel/release#policy
  deprecate! date: "2026-08-25", because: :unsupported
  disable! date: "2027-08-25", because: :unsupported

  depends_on "go" => :build

  deny_network_access!

  def install
    libexec.install Dir["*"]

    cd libexec/"src" do
      if OS.windows?
        # make.bash refuses to run on Windows; make.bat is its Windows counterpart.
        ENV["GOROOT_BOOTSTRAP"] = Formula["go"].opt_libexec
        # Run directly: `system "cmd", "/c", "make.bat"` left cmd at its prompt on CI without running make.bat.
        system "./make.bat"
      else
        # Set portable defaults for CC/CXX to be used by cgo
        with_env(CC: "cc", CXX: "c++") { system "./make.bash" }
      end
    end

    bin.install_symlink Dir[libexec/"bin/go*"]

    # Remove useless files.
    # Breaks patchelf because folder contains weird debug/test files
    rm_r(libexec/"src/debug/elf/testdata")
    # Binaries built for an incompatible architecture
    rm_r(libexec/"src/runtime/pprof/testdata")
    # Remove testdata with binaries for non-native architectures.
    rm_r(libexec/"src/debug/dwarf/testdata")
  end

  test do
    (testpath/"hello.go").write <<~GO
      package main

      import "fmt"

      func main() {
          fmt.Println("Hello World")
      }
    GO

    # Run go fmt check for no errors then run the program.
    # This is a a bare minimum of go working as it uses fmt, build, and run.
    system bin/"go", "fmt", "hello.go"
    assert_equal "Hello World\n", shell_output("#{bin}/go run hello.go")

    with_env(GOOS: "freebsd", GOARCH: "amd64") do
      system bin/"go", "build", "hello.go"
    end

    (testpath/"hello_cgo.go").write <<~GO
      package main

      /*
      #include <stdlib.h>
      #include <stdio.h>
      void hello() { printf("%s\\n", "Hello from cgo!"); fflush(stdout); }
      */
      import "C"

      func main() {
          C.hello()
      }
    GO

    # Try running a sample using cgo without CC or CXX set to ensure that the
    # toolchain's default choice of compilers work
    with_env(CC: nil, CXX: nil, CGO_ENABLED: "1") do
      assert_equal "Hello from cgo!\n", shell_output("#{bin}/go run hello_cgo.go")
    end
  end
end
