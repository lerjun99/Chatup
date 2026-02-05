using AutoMapper;
using ChatUp.Application.Auth.Commands;
using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.Client.Commands;
using ChatUp.Application.Features.Client.DTOs;
using ChatUp.Application.Features.User.DTOs;
using ChatUp.Application.Features.User.Queries;
using ChatUp.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChatUp.Api.Controllers
{
    //[Authorize("ApiKey")]
    [Route("[controller]/[action]")]
    [ApiController]
    public class ClientController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IClientRepository _clientRepository;
        private readonly IMapper _mapper;
        public ClientController(IMediator mediator, IClientRepository clientRepository, IMapper mapper)
        {
            _mediator = mediator;
            _clientRepository = clientRepository;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClientDto>>> GetAll()
        {
            var clients = await _clientRepository.GetAllAsync();
            return Ok(_mapper.Map<IEnumerable<ClientDto>>(clients));
        }
        [HttpGet("assigned/{userId}")]
        public async Task<IActionResult> GetAssignedClients(int userId)
        {
            var result = await _mediator.Send(new GetUserAssignedClientsCommand(userId));
            return Ok(result);
        }
        [HttpGet("{id}")]
        public async Task<ActionResult<ClientDto>> GetById(int id)
        {
            var client = await _clientRepository.GetByIdAsync(id);
            if (client == null) return NotFound();
            return Ok(_mapper.Map<ClientDto>(client));
        }

        [HttpPost]
        public async Task<ActionResult<ClientDto>> Create(ClientDto clientDto)
        {
            var client = _mapper.Map<Client>(clientDto);
            var created = await _clientRepository.AddAsync(client);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, _mapper.Map<ClientDto>(created));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, ClientDto clientDto)
        {
            if (id != clientDto.Id) return BadRequest();

            var client = _mapper.Map<Client>(clientDto);
            await _clientRepository.UpdateAsync(client);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _clientRepository.DeleteAsync(id);
            return NoContent();
        }
    }
}
