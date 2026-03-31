# 🐧 PenguinSpace – Project Proposal

---

# Overview

**PenguinSpace** là một desktop application và automation tool giúp developer **thu hồi (reclaim), tối ưu và quản lý dung lượng ổ đĩa** bị chiếm bởi **WSL distros và Docker storage** trên Windows.

Khác với các tool monitor disk thông thường, PenguinSpace tập trung vào việc **reclaim disk space nhanh nhất có thể** bằng các phương pháp hiệu quả như:

* Reset distro
* Repack distro
* Cleanup Docker storage
* Compact VHDX
* Storage analysis
* Automation qua CLI
* Điều khiển qua AI (MCP)

Mục tiêu của PenguinSpace:

> Giúp developer reclaim disk space bị WSL và Docker “ăn mất” trên Windows một cách nhanh chóng, an toàn và tự động hóa.

---

# Problem / Pain Points

## Vấn đề thực tế khi dùng WSL + Docker

Rất nhiều developer trên Windows sử dụng:

* WSL
* Docker Desktop
* Node / Python / .NET
* Database containers
* Build artifacts
* Package cache
* Dev environments

Sau một thời gian, ổ C bị đầy nhưng **không biết nguyên nhân và không biết dọn như thế nào**.

---

## Pain points chính

### 1. WSL ext4.vhdx phình to

WSL lưu toàn bộ filesystem trong file:

```
ext4.vhdx
```

Khi xoá file trong Linux:

* Dung lượng trong Linux giảm
* Nhưng dung lượng Windows **không giảm**

Muốn shrink phải dùng:

```
wsl --shutdown
diskpart
compact vdisk
```

→ Quy trình thủ công, nhiều developer không biết.

---

### 2. Docker storage rất lớn

Docker images, layers, volumes nằm trong:

```
docker-desktop-data ext4.vhdx
```

Dung lượng có thể lên:

* 30GB
* 50GB
* 100GB+

Nhưng Docker Desktop không có disk usage manager rõ ràng.

---

### 3. Cách reclaim disk space hiệu quả rất thủ công

Các cách reclaim disk space thực tế:

| Method                          | Hiệu quả |
| ------------------------------- | -------- |
| Xoá cache trong distro          | nhỏ      |
| docker prune                    | vừa      |
| compact vhdx                    | vừa      |
| export → import distro          | lớn      |
| unregister distro → install lại | rất lớn  |

Nhưng các bước này:

* Phải dùng CLI
* Phải nhớ nhiều lệnh
* Có nguy cơ mất dữ liệu nếu làm sai
* Không có UI
* Không có tool tổng hợp

---

### 4. Developer không biết distro nào đang chiếm dung lượng

Có thể có nhiều distro:

* Ubuntu
* Debian
* Kali
* docker-desktop
* docker-desktop-data

Windows không hiển thị size rõ ràng.

---

# Situation

Hiện tại:

* Windows không có tool quản lý WSL disk usage
* WSL CLI không có disk management tools
* Docker Desktop không có storage reclaim tool mạnh
* Shrink VHDX phải dùng diskpart thủ công
* Reset distro và repack distro phải dùng CLI
* Không có tool nào tập trung vào **reclaim disk space từ WSL và Docker**

=> Đây là một khoảng trống tool khá rõ ràng.

---

# Proposed Solution – PenguinSpace

PenguinSpace sẽ cung cấp một tool giúp developer:

* Xem distro nào đang chiếm dung lượng
* Reset distro
* Repack distro (export → import)
* Cleanup Docker storage
* Compact VHDX
* Shutdown WSL
* Thu hồi dung lượng ổ đĩa chỉ bằng vài nút bấm hoặc CLI command
* Cho phép AI agent điều khiển qua MCP

### PenguinSpace không phải:

* WSL manager
* Docker manager
* Disk viewer
* System monitor

### PenguinSpace là:

> Tool để reclaim disk space từ WSL và Docker trên Windows, có thể điều khiển qua Desktop UI, CLI hoặc AI agent.

---

# Core Features (v1)

## PenguinSpace v1 tập trung vào reclaim disk space

| Feature             | Mô tả                                        |
| ------------------- | -------------------------------------------- |
| List WSL distros    | Hiển thị Ubuntu, Debian, docker-desktop-data |
| Show ext4.vhdx size | Distro nào đang chiếm dung lượng             |
| Reset distro        | unregister + install lại                     |
| Repack distro       | export → unregister → import lại             |
| Docker cleanup      | docker system prune                          |
| Compact VHDX        | shrink ext4.vhdx                             |
| Shutdown WSL        | wsl --shutdown                               |
| Open distro folder  | Explorer                                     |
| Refresh             | Reload size                                  |

### Core Actions của PenguinSpace

| Action        | Mục tiêu                         |
| ------------- | -------------------------------- |
| Reset distro  | Xoá sạch distro để reclaim space |
| Repack distro | Shrink filesystem nhưng giữ data |
| Docker prune  | Xoá images/volumes               |
| Compact VHDX  | Shrink disk                      |
| Shutdown WSL  | Required step                    |

Đây là các tính năng cốt lõi của PenguinSpace.

---

# Technical Approach

PenguinSpace không cần kernel driver hay low-level integration.
Chỉ cần sử dụng:

* WSL CLI
* Docker CLI
* File system access
* Diskpart compact vdisk
* Desktop UI
* CLI interface
* MCP server

## Các command PenguinSpace sẽ sử dụng

| Action         | Command                  |
| -------------- | ------------------------ |
| List distro    | `wsl -l -v`              |
| Shutdown WSL   | `wsl --shutdown`         |
| Reset distro   | `wsl --unregister`       |
| Export distro  | `wsl --export`           |
| Import distro  | `wsl --import`           |
| Docker cleanup | `docker system prune -a` |
| Compact vhdx   | `diskpart compact vdisk` |
| Get size       | File system              |
| Open folder    | explorer.exe             |

PenguinSpace thực chất là:

> Automation tool + UI + CLI wrapper cho các WSL và Docker disk operations.

---

# Technical Architecture

## High-level architecture

```
                    +-------------------+
                    |   Desktop UI      |
                    |    (Avalonia)     |
                    +---------+---------+
                              |
                    +---------v---------+
                    | PenguinSpace Core |
                    +---------+---------+
                              |
        +-----------+---------+----------+
        |           |                    |
   WSL Service   Docker Service     Disk Service
        |           |                    |
        +-----------+---------+----------+
                              |
                        Command Runner
                              |
                              OS

                    +-------------------+
                    | PenguinSpace CLI  |
                    +-------------------+

                    +-------------------+
                    | PenguinSpace MCP  |
                    |     Server        |
                    +-------------------+
```

---

# Modules

| Module           | Responsibility         |
| ---------------- | ---------------------- |
| WSL Service      | list distro, shutdown  |
| Distro Service   | reset, export, import  |
| Disk Service     | compact vhdx           |
| Docker Service   | prune images/volumes   |
| Storage Analyzer | disk usage             |
| Command Runner   | run CLI commands       |
| CLI              | command line interface |
| MCP              | MCP server             |
| UI               | desktop UI             |
| Logging          | logs                   |
| Backup Service   | export/import distro   |

---

# Suggested Tech Stack

## Desktop App & Core

| Layer     | Tech                          |
| --------- | ----------------------------- |
| Language  | C#                            |
| Framework | .NET                          |
| UI        | Avalonia                      |
| Pattern   | MVVM                          |
| Logging   | Serilog                       |
| Charts    | LiveCharts                    |
| Build     | dotnet CLI                    |
| Publish   | Self-contained exe            |
| CLI       | System.CommandLine            |
| MCP       | MCP server (JSON RPC / stdio) |

Avalonia được chọn vì:

* Dev bằng VSCode
* Build bằng CLI
* Self-contained publish
* Không cần MSIX
* Không cần Windows App SDK
* Phù hợp AI-driven development

---

# Solution Structure

```
PenguinSpace.sln
 ├── PenguinSpace.Core
 ├── PenguinSpace.Application
 ├── PenguinSpace.Infrastructure
 ├── PenguinSpace.WSL
 ├── PenguinSpace.Docker
 ├── PenguinSpace.Disk
 ├── PenguinSpace.CLI
 ├── PenguinSpace.MCP
 └── PenguinSpace.UI
```

---

# MCP Integration

PenguinSpace sẽ expose MCP tools để AI agent có thể thao tác với WSL và Docker storage.

### MCP Tools

| Tool             | Description        |
| ---------------- | ------------------ |
| list_wsl_distros | List WSL distros   |
| get_distro_size  | Get disk usage     |
| reset_distro     | Reset distro       |
| repack_distro    | Repack distro      |
| docker_prune     | Cleanup docker     |
| compact_vhdx     | Compact disk       |
| shutdown_wsl     | Shutdown WSL       |
| export_distro    | Backup distro      |
| import_distro    | Restore distro     |
| analyze_storage  | Analyze disk usage |

PenguinSpace có thể trở thành **system maintenance tool cho AI agents**.

---

# Long-term Vision

PenguinSpace có thể evolve thành:

```
Developer Environment Storage Manager
 ├── WSL
 ├── Docker
 ├── VM disks
 ├── Dev caches
 ├── Package caches
 ├── Build artifacts
 ├── Storage history
 ├── Backup
 └── AI-controlled maintenance (MCP)
```

Mục tiêu dài hạn:

> PenguinSpace becomes the storage manager and maintenance tool for developer environments, accessible via Desktop UI, CLI, and AI agents.

---

# Summary

## PenguinSpace solves:

| Problem                    | Solution                  |
| -------------------------- | ------------------------- |
| WSL eats disk              | Reset / Repack distro     |
| ext4.vhdx too big          | Compact VHDX              |
| Docker storage huge        | Docker cleanup            |
| Hard to reclaim disk space | UI + CLI + MCP automation |
| Hard to manage distro      | Desktop tool              |
| Disk usage unclear         | Storage analyzer          |
| Manual maintenance         | Automation                |
| AI cannot manage storage   | MCP integration           |

---

# Final Definition

> PenguinSpace is a developer storage management tool that helps reclaim disk space used by WSL and Docker, and can be controlled via Desktop UI, CLI, or AI agents through MCP.