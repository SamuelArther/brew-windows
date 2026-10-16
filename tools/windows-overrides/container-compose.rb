class ContainerCompose < Formula
  desc "Manage WSL2 containers with Docker Compose files"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "container-compose", "container-compose"
  end

  def caveats
    <<~EOS
      Services run as WSL2 containers through the container formula's store, so WSL must be installed (wsl --install).
    EOS
  end

  test do
    output = shell_output("#{bin}/container-compose down 2>&1", 1)
    assert_match "compose.yml not found at", output
    (testpath/"compose.yml").write <<~YAML
      services:
        web:
          image: nginx:alpine
          ports:
            - "8080:80"
          environment:
            GREETING: hello
          depends_on: [db]
        db:
          image: postgres:16
          command: postgres -c 'max_connections=50'
    YAML
    output = shell_output("#{bin}/container-compose -p demo config")
    assert_match "container: demo-web", output
    assert_match "GREETING=hello", output
    assert_match "command: [postgres, -c, max_connections=50]", output
    assert_match "container-compose version", shell_output("#{bin}/container-compose version")
  end
end
