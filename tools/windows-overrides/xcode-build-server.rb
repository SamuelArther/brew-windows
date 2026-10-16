class XcodeBuildServer < Formula
  desc "Build server protocol implementation for integrating Xcode with sourcekit-lsp"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "xcode-build-server", "xcode-build-server"
  end

  test do
    system bin/"xcode-build-server", "--help"
    system bin/"xcode-build-server", "config", "-project", "App.xcodeproj", "-scheme", "App"
    assert_match "\"scheme\": \"App\"", (testpath/"buildServer.json").read
    log = <<~LOG
      CompileC /tmp/foo.o /src/App/Foo.m normal arm64 objective-c com.apple.compilers.llvm.clang.1_0.compiler (in target 'App' from project 'App')
          cd /src/App
          /usr/bin/clang -x objective-c -fobjc-arc -c /src/App/Foo.m -o /tmp/foo.o
    LOG
    pipe_output("#{bin}/xcode-build-server parse", log, 0)
    assert_match "/src/App/Foo.m", (testpath/".compile").read
    request = '{"jsonrpc":"2.0","id":1,"method":"build/initialize","params":{"capabilities":{}}}'
    exit_request = '{"jsonrpc":"2.0","method":"build/exit"}'
    input = "Content-Length: #{request.bytesize}\r\n\r\n#{request}" \
            "Content-Length: #{exit_request.bytesize}\r\n\r\n#{exit_request}"
    assert_match "\"bspVersion\":\"2.0\"", pipe_output(bin/"xcode-build-server", input, 0)
  end
end
